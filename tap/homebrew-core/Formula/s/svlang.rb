class Svlang < Formula
  desc "SystemVerilog compiler and language services"
  homepage "https://sv-lang.com/"
  url "https://github.com/MikePopoloski/slang/archive/refs/tags/v12.0.tar.gz"
  sha256 "64b3eb9d38ee126e009cbb8da0cfa6f68d970334e52ba084ad7c68e4b5fa804c"
  license "MIT"
  head "https://github.com/MikePopoloski/slang.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4bb1fdadae01c850908d8c0802c2bab912c3d571bafcc7b0de0f362035237e1a"
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
