class Ensmallen < Formula
  desc "Flexible C++ library for efficient mathematical optimization"
  homepage "https://ensmallen.org"
  url "https://github.com/mlpack/ensmallen/archive/refs/tags/3.11.1.tar.gz"
  sha256 "3606c0d0a06ac75858ddb4eecd0802154bc91d45763aa9521cf53a0e78178d55"
  license "BSD-3-Clause"
  compatibility_version 1
  head "https://github.com/mlpack/ensmallen.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "60384fa598e4dcd13c34a33e6337d94e3187ea3ab56a2f58e51e2094282d4409"
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
