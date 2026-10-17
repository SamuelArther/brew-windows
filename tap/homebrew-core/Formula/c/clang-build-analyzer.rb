class ClangBuildAnalyzer < Formula
  desc "Tool to analyze compilation time"
  homepage "https://github.com/aras-p/ClangBuildAnalyzer"
  url "https://github.com/aras-p/ClangBuildAnalyzer/archive/refs/tags/v1.6.0.tar.gz"
  sha256 "868a8d34ecb9b65da4e5874342062a12c081ce4385c7ddd6ce7d557a0c5c292d"
  license all_of: ["Unlicense", "Zlib", "MIT", "BSL-1.0", "BSD-3-Clause", "Apache-2.0",
                   "BSD-2-Clause", "Apache-2.0" => { with: "LLVM-exception" }]
  head "https://github.com/aras-p/ClangBuildAnalyzer.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "399859465522a1d70019a3f9f109860c30de4749869e0aa5a9c280d606a46ae5"
  end

  depends_on "cmake" => :build
  uses_from_macos "llvm"

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cxx").write <<~CPP
      int main() {}
    CPP
    ENV.clang
    system ENV.cxx, "-c", "-ftime-trace", testpath/"test.cxx"
    system bin/"ClangBuildAnalyzer", "--all", testpath, "test.db"
    system bin/"ClangBuildAnalyzer", "--analyze", "test.db"
  end
end
