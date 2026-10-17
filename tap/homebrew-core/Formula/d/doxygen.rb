class Doxygen < Formula
  desc "Generate documentation for several programming languages"
  homepage "https://www.doxygen.nl/"
  url "https://doxygen.nl/files/doxygen-1.18.0.src.tar.gz"
  mirror "https://downloads.sourceforge.net/project/doxygen/rel-1.18.0/doxygen-1.18.0.src.tar.gz"
  sha256 "a1deed70a6785bbec95a2b2a9e419dc7f7b223a9d74a8644ae611c8e2dcdd354"
  license "GPL-2.0-only"
  compatibility_version 1
  head "https://github.com/doxygen/doxygen.git", branch: "master"

  livecheck do
    url "https://www.doxygen.nl/download.html"
    regex(/href=.*?doxygen[._-]v?(\d+(?:\.\d+)+)[._-]src\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "4c83322e96ee24b78a9339da5124fdaa1c1130c3838cd5db9a6905d50f0d7f1b"
  end

  depends_on "bison" => :build
  depends_on "cmake" => :build
  depends_on "fmt"
  depends_on "spdlog"

  uses_from_macos "flex" => :build, since: :big_sur
  uses_from_macos "python" => :build
  uses_from_macos "sqlite"

  def install
    # Remove bundled dependencies
    rm_r(%w[
      deps/fmt
      deps/spdlog
      deps/sqlite3
    ])

    args = %W[
      -DPython_EXECUTABLE=#{which("python3")}
      -Duse_sys_fmt=ON
      -Duse_sys_spdlog=ON
      -Duse_sys_sqlite3=ON
    ]

    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    system bin/"doxygen", "-g"
    system bin/"doxygen", "Doxyfile"
  end
end
