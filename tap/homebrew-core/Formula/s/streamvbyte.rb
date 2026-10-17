class Streamvbyte < Formula
  desc "Fast integer compression in C"
  homepage "https://fast-pack.github.io/streamvbyte/"
  url "https://github.com/fast-pack/streamvbyte/archive/refs/tags/v3.0.1.tar.gz"
  sha256 "b19188f66065553350a716346580188c0c33e40667b006a68ec25cbde6c31bf4"
  license "Apache-2.0"
  head "https://github.com/fast-pack/streamvbyte.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/streamvbyte-3.0.1"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "08287af877b7b178fa95ff50edf8a9f6abe9e69ed3088c68ce36720dd437118c"
  end

  depends_on "cmake" => :build

  def install
    system "cmake", "-S", ".", "-B", "build", "-DCMAKE_POLICY_VERSION_MINIMUM=3.5", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    pkgshare.install "examples/example.c"
  end

  test do
    system ENV.cc, pkgshare/"example.c", "-I#{include}", "-L#{lib}", "-lstreamvbyte", "-o", "test"
    system testpath/"test"
  end
end
