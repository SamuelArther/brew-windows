class Svlang < Formula
  desc "SystemVerilog compiler and language services"
  homepage "https://sv-lang.com/"
  url "https://github.com/MikePopoloski/slang/archive/refs/tags/v12.2.tar.gz"
  sha256 "29c8dee801e489e10f84ebc8a65b641bbcee18614fd183223b35e85c2461b6ab"
  license "MIT"
  head "https://github.com/MikePopoloski/slang.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7832f1c1f46299ef605cd92e2af584c48031b8bf7c619ea9be272bc92b4f0068"
  end

  depends_on "cmake" => :build
  depends_on "boost"
  depends_on "fmt"
  depends_on "mimalloc"
  depends_on "tomlplusplus"

  uses_from_macos "python" => :build

  on_macos do
    depends_on "llvm" => :build if DevelopmentTools.clang_build_version <= 1600
  end

  # Needs std::views::join, missing from the macOS 14 SDK's libc++
  fails_with :clang do
    build 1600
    cause "needs std::views::join, missing from the macOS 14 SDK's libc++"
  end

  deny_network_access!

  def install
    args = %w[
      -DHOMEBREW_ALLOW_FETCHCONTENT=ON
      -DFETCHCONTENT_FULLY_DISCONNECTED=ON
      -DFETCHCONTENT_TRY_FIND_PACKAGE_MODE=ALWAYS
      -DSLANG_INCLUDE_TESTS=OFF
      -DSLANG_INCLUDE_TOOLS=ON
      -DSLANG_USE_SYSTEM_BOOST=ON
      -DSLANG_USE_SYSTEM_FMT=ON
      -DSLANG_USE_SYSTEM_TOMLPLUSPLUS=ON
    ]

    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.sv").write <<~SV
      module top;
        initial begin
          $display("Hello, Slang!");
        end
      endmodule
    SV
    system bin/"slang", "test.sv"
  end
end
