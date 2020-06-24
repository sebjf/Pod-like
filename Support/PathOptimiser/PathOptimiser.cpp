// g2o - General Graph Optimization
// Copyright (C) 2012 R. Kümmerle
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
// * Redistributions of source code must retain the above copyright notice,
//   this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright
//   notice, this list of conditions and the following disclaimer in the
//   documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
// IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#include <Eigen/Core>
#include <Eigen/StdVector>
#include <Eigen/Geometry>
#include <iostream>
#include <cmath>

#include "g2o/stuff/sampler.h"
#include "g2o/stuff/command_args.h"
#include "g2o/core/sparse_optimizer.h"
#include "g2o/core/block_solver.h"
#include "g2o/core/solver.h"
#include "g2o/core/optimization_algorithm_levenberg.h"
#include "g2o/core/optimization_algorithm_gauss_newton.h"
#include "g2o/core/base_vertex.h"
#include "g2o/core/base_unary_edge.h"
#include "g2o/core/base_multi_edge.h"
#include "g2o/core/base_binary_edge.h"
#include "g2o/solvers/csparse/linear_solver_csparse.h"

using namespace std;

int repeat(int k, int n) //https://stackoverflow.com/questions/1082917/
{
	return ((k %= n) < 0) ? k + n : k;
}

double clamp(double dp, double range) // between -1 and 1, for acos and track width
{
	return std::max(-range, std::min(range, dp));
}

double clamp(double dp, double min, double max) // between -1 and 1, for acos and track width
{
	return std::max(min, std::min(max, dp));
}

// Defines the position along the track section. 
// Each vertex has its own section defined by the limits of the track edge (in 3D)
class Vertex : public g2o::BaseVertex<1, double>
{
public:
	EIGEN_MAKE_ALIGNED_OPERATOR_NEW;
	Vertex()
	{
	}

	Eigen::Vector3d upper;
	Eigen::Vector3d lower;

	Eigen::Vector3d position()
	{
		// the position should be a direct interpolation; this does affect the solve.
		return (1 - estimate()) * lower + estimate() * upper;
	}

	virtual bool read(std::istream& /*is*/)
	{
		cerr << __PRETTY_FUNCTION__ << " not implemented yet" << endl;
		return false;
	}

	virtual bool write(std::ostream& /*os*/) const
	{
		cerr << __PRETTY_FUNCTION__ << " not implemented yet" << endl;
		return false;
	}

	virtual void setToOriginImpl()
	{
		cerr << __PRETTY_FUNCTION__ << " not implemented yet" << endl;
	}

	virtual void oplusImpl(const double* update)
	{
		_estimate += update[0];

		// clamp here or make constraint?
		_estimate = clamp(_estimate, 0.20, 0.80);
	}
};

class DistanceEdge : public g2o::BaseBinaryEdge<1, double, Vertex, Vertex>
{
public:
	EIGEN_MAKE_ALIGNED_OPERATOR_NEW;

	virtual bool read(std::istream& /*is*/)
	{
		cerr << __PRETTY_FUNCTION__ << " not implemented yet" << endl;
		return false;
	}

	virtual bool write(std::ostream& /*os*/) const
	{
		cerr << __PRETTY_FUNCTION__ << " not implemented yet" << endl;
		return false;
	}

	void computeError()
	{
		auto X = static_cast<Vertex*>(vertex(1))->position();
		auto Y = static_cast<Vertex*>(vertex(0))->position();
		_error(0) = (X - Y).norm();
	}
};

class MengerCurvatureEdge : public g2o::BaseMultiEdge<1, double>
{
public:
	EIGEN_MAKE_ALIGNED_OPERATOR_NEW;
	MengerCurvatureEdge()
	{
		weight = 1.0;
		section = -1;
	}

	double weight;
	int section;

	virtual bool read(std::istream& /*is*/)
	{
		cerr << __PRETTY_FUNCTION__ << " not implemented yet" << endl;
		return false;
	}

	virtual bool write(std::ostream& /*os*/) const
	{
		cerr << __PRETTY_FUNCTION__ << " not implemented yet" << endl;
		return false;
	}

	void computeError()
	{
		auto X = static_cast<Vertex*>(vertex(2))->position();
		auto Y = static_cast<Vertex*>(vertex(1))->position();
		auto Z = static_cast<Vertex*>(vertex(0))->position();

		// poor mans projection into XZ
		X(1) = 0;
		Y(1) = 0;
		Z(1) = 0;

		auto YX = X - Y;
		auto YZ = Z - Y;	
		auto ZX = X - Z;

		// https://en.wikipedia.org/wiki/Menger_curvature

		auto dp = YX.normalized().dot(YZ.normalized());
		auto len = ZX.norm();
		double curvature = (2.0 * sin(acos(clamp(dp, 1.0)))) / len; // dot may give values a teeny bit outside -1..1, and acos throws an exception on these

		assert(!isnan(curvature));

		// if the three points are asymmetric enough, the len term can begin to undermine the sine term,
		// so if angle is acute add an extra penalty
		auto modifier = clamp(dp, 0, 1);

		_error(0) = (curvature + modifier) * weight; // curvature would ideally be zero
	}

	/*
	virtual void linearizeOplus()
	{		
		//_jacobianOplus[ vertex ].(error dimension, vertex dimension)
	}
	*/
};

double errorOfSolution(g2o::OptimizableGraph &graph)
{
	auto error = 0.0;
	auto edges = graph.edges();
	auto it = edges.begin();
	while (it != edges.end())
	{
		auto curvature = dynamic_cast<MengerCurvatureEdge*>(*it);
		if (curvature != nullptr)
		{
			curvature->computeError();
			error += curvature->error()(0);
		}
		auto distance = dynamic_cast<DistanceEdge*>(*it);
		if(distance != nullptr)
		{ 
			distance->computeError();
			error += distance->error()(0);
		}
		it++;
	}
	return error;
}

std::vector<double> readfloats(std::string filename)
{
	std::fstream in(filename);
	std::vector<double> v;
	std::string line;
	while (std::getline(in, line))
	{
		std::stringstream ss(line);
		double value;
		ss >> value;
		v.push_back(value);
	}
	return v;
}

int main(int argc, char** argv)
{
	int maxIterations;
	bool verbose;
	std::string pathFilename;
	std::string weightsFilename;
	int mode;

	g2o::CommandArgs arg;
	arg.param("i", maxIterations, 10, "perform n iterations");
	arg.param("v", verbose, false, "verbose output of the optimization process");
	arg.param("p", pathFilename, "", "path as sections");
	arg.param("o", weightsFilename, "", "output weights");
	arg.param("m", mode, 0, "what property to optimise");

	arg.parseArgs(argc, argv);

	// read the data

	auto v = readfloats(pathFilename);

	// and transform

	struct section
	{
		EIGEN_MAKE_ALIGNED_OPERATOR_NEW
		Eigen::Vector3d lower;
		Eigen::Vector3d upper;
		double jump;
	};
	
	std::vector<section> sections;
	for (size_t i = 0; i < v.size();)
	{
		section s;
		s.lower(0) = v[i++];
		s.lower(1) = v[i++];
		s.lower(2) = v[i++];
		s.upper(0) = v[i++];
		s.upper(1) = v[i++];
		s.upper(2) = v[i++];
		s.jump = v[i++];
		sections.push_back(s);
	}

	// some handy typedefs
	typedef g2o::BlockSolver< g2o::BlockSolverTraits<Eigen::Dynamic, Eigen::Dynamic> >  MyBlockSolver;
	typedef g2o::LinearSolverCSparse<MyBlockSolver::PoseMatrixType> MyLinearSolver;

	// setup the solver
	g2o::SparseOptimizer optimizer;
	optimizer.setVerbose(true);
	g2o::OptimizationAlgorithmLevenberg* solver = new g2o::OptimizationAlgorithmLevenberg(
		g2o::make_unique<MyBlockSolver>(g2o::make_unique<MyLinearSolver>()));
	optimizer.setAlgorithm(solver);

	auto numnodes = sections.size();

	// Create vertices for each node in path
	for (size_t i = 0; i < numnodes; i++)
	{
		auto vertex = new Vertex();
		vertex->setId(i);
		vertex->upper = sections[i].upper;
		vertex->lower = sections[i].lower;
		vertex->setEstimate(0.5);
		optimizer.addVertex(vertex);
	}

	for (size_t i = 0; i < numnodes; i++)
	{
		int previous = repeat(i - 1, numnodes);
		int current = i;
		int next = repeat(i + 1, numnodes);

		auto edge = new MengerCurvatureEdge();
		edge->setId(i);
		edge->setMeasurement(0);
		edge->setInformation(Eigen::Matrix<double, 1, 1>::Identity());
		edge->resize(3);
		edge->setVertex(0, optimizer.vertex(previous));
		edge->setVertex(1, optimizer.vertex(current));
		edge->setVertex(2, optimizer.vertex(next));
		edge->section = current;
		edge->weight += sections[current].jump * 2.0; // weight the jump sections high
		optimizer.addEdge(edge);
	}

	if (mode == 1)
	{
		auto it = optimizer.edges().begin();
		while (it != optimizer.edges().end())
		{
			// the menger curvature is so small compared to even a trivial distance we must weight the constraint for it to have an effect.
			// menger curvature is physically based, so once we find an ideal parameter (analogous to a minimum radius) it will be correct regardless
			// of sampling resolution, etc
			auto edge = dynamic_cast<MengerCurvatureEdge*>(*it);
			edge->weight += 50.0 + sections[edge->section].jump * 500.0;
			it++;
		}

		for (size_t i = 0; i < numnodes; i++)
		{
			int current = i;
			int next = repeat(i + 1, numnodes);

			auto edge = new DistanceEdge();
			edge->setId(i);
			edge->setMeasurement(0);
			edge->setInformation(Eigen::Matrix<double, 1, 1>::Identity());
			edge->setVertex(0, optimizer.vertex(current));
			edge->setVertex(1, optimizer.vertex(next));
			optimizer.addEdge(edge);
		}
	}

	cout << "Original Measure (Curvature or Distance): " << errorOfSolution(optimizer) << endl;

	// perform the optimization
	optimizer.initializeOptimization();
	optimizer.setVerbose(verbose);
	optimizer.optimize(maxIterations);

	if (verbose)
		cout << endl;

	// print out the result
	cout << "Revised Measure (Curvature or Distance): " << errorOfSolution(optimizer) << endl;

	if (weightsFilename.length() > 0) {
		ofstream output;
		output.open(weightsFilename);
		for (size_t i = 0; i < numnodes; i++)
		{
			output << static_cast<Vertex*>(optimizer.vertex(i))->estimate() << endl;
		}
		output.close();
	}

	return 0;
}
