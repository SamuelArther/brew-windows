class Ensmallen < Formula
  desc "Flexible C++ library for efficient mathematical optimization"
  homepage "https://ensmallen.org"
  url "https://github.com/mlpack/ensmallen/archive/refs/tags/3.11.2.tar.gz"
  sha256 "e2fb8eef7c5a8e1162774189db3083f78615beeb832e7e053b175fc3003e7ac7"
  license "BSD-3-Clause"
  compatibility_version 1
  head "https://github.com/mlpack/ensmallen.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e09050dbde3f9f2adc47516029d887e7f3d3150f3907af9e759695c3b8c97784"
  end

  depends_on "cmake" => :build
  depends_on "armadillo"

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <ensmallen.hpp>
      using namespace ens;
      int main()
      {
        test::RosenbrockFunction f;
        arma::mat coordinates = f.GetInitialPoint();
        Adam optimizer(0.001, 32, 0.9, 0.999, 1e-8, 3, 1e-5, true);
        optimizer.Optimize(f, coordinates);
        return 0;
      }
    CPP

    system ENV.cxx, "test.cpp", "-std=c++14", "-I#{include}", "-L#{formula_opt_lib("armadillo")}",
                    "-larmadillo", "-o", "test"
  end
end
