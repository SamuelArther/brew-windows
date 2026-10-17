class Slicot < Formula
  desc "Fortran subroutines library for systems and control"
  homepage "https://www.slicot.org/"
  url "https://github.com/SLICOT/SLICOT-Reference/releases/download/v5.9.1/slicot-5.9.1.tar.gz"
  sha256 "0f812933a07577db8c80dc53fc3663844114d43880f4c2fb383622891e931504"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b9efaded4a58744ff40d0d6a24ccc6be96e139dc808993a4d439223ddba9e67f"
  end

  depends_on "cmake" => :build
  depends_on "gcc" # for gfortran
  depends_on "openblas"

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build",
                    "-DBUILD_SHARED_LIBS=ON",
                    "-DSLICOT_TESTING=OFF",
                    *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    # Using a test without 0.0000 which can fail from sign swapping to -0.0000
    pkgshare.install "examples/TAB05RD.f", "examples/data/AB05RD.dat", "examples/results/AB05RD.res"
  end

  test do
    system "gfortran", "-o", "test", pkgshare/"TAB05RD.f",
                       "-L#{lib}", "-lslicot", "-L#{formula_opt_lib("openblas")}", "-lopenblas"
    assert_equal (pkgshare/"AB05RD.res").read, pipe_output("./test", (pkgshare/"AB05RD.dat").read, 0)
  end
end
