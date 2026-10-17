class Openfast < Formula
  desc "NREL-supported OpenFAST whole-turbine simulation code"
  homepage "https://openfast.readthedocs.io"
  url "https://github.com/openfast/openfast.git",
      tag:      "v5.0.0",
      revision: "2895884d2be01862173c88d70f86b358d2f1a50a"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "aca38870c491dbf65dc5ce15b66496b63b20713cb9c7c6edebc90e5a8dcf9b67"
  end

  depends_on "cmake" => :build
  depends_on "gcc" # for gfortran
  depends_on "openblas"

  def install
    args = %w[
      -DDOUBLE_PRECISION=OFF
      -DBLA_VENDOR=OpenBLAS
    ]

    system "cmake", "-S", ".", "-B", ".", *args, *std_cmake_args
    system "cmake", "--build", ".", "--target", "openfast"
    bin.install "glue-codes/openfast/openfast"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/openfast -h")
  end
end
