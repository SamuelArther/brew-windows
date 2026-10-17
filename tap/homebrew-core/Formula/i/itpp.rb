class Itpp < Formula
  desc "Library of math, signal, and communication classes and functions"
  homepage "https://itpp.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/itpp/itpp/4.3.1/itpp-4.3.1.tar.bz2"
  sha256 "50717621c5dfb5ed22f8492f8af32b17776e6e06641dfe3a3a8f82c8d353b877"
  license "GPL-3.0-or-later"
  head "https://git.code.sf.net/p/itpp/git.git", branch: "master"

  livecheck do
    url :stable
    regex(%r{url=.*?/itpp[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "21cd8be7b3c2649f6bbe177b0301921bc1d41efe6a56fe8202549b7662e2f75d"
  end

  depends_on "cmake" => :build
  depends_on "fftw"

  deny_network_access!

  def install
    # Rename VERSION file to avoid build failure: version:1:1: error: expected unqualified-id
    # Reported upstream at: https://sourceforge.net/p/itpp/bugs/262/
    mv "VERSION", "VERSION.txt"

    args = %w[-DCMAKE_POLICY_VERSION_MINIMUM=3.5]
    # Upstream only adds the OpenMP compile flags, so with `libomp` (via `fftw`) found the link fails on macOS
    args << "-DCMAKE_DISABLE_FIND_PACKAGE_OpenMP=ON" if OS.mac?

    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <itpp/itcomm.h>
      #include <iostream>

      int main() {
        itpp::BPSK bpsk;
        itpp::bvec input_bits = "0 1 0 1";
        itpp::vec modulated_signal;
        bpsk.modulate_bits(input_bits, modulated_signal);
        std::cout << "Modulated signal: " << modulated_signal << std::endl;
        return 0;
      }
    CPP

    system ENV.cxx, "test.cpp", "-o", "test", "-I#{include}", "-L#{lib}", "-litpp"
    system "./test"
  end
end
