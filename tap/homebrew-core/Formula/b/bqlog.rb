class Bqlog < Formula
  desc "Cross-language logging library"
  homepage "https://github.com/Tencent/BqLog"
  url "https://github.com/Tencent/BqLog/archive/refs/tags/Release_2.7.1.tar.gz"
  sha256 "39678545fe7322d2193ff5e22a6e99937de1b6dc01a0531f31440991fa4881a9"
  license "Apache-2.0"
  head "https://github.com/Tencent/BqLog.git", branch: "main"

  livecheck do
    url :stable
    regex(/^Release[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "257c14c3b66022dfda2fe68204f78dfcce7a1566104ea61378547343ce672a31"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    args = %w[-DBUILD_LIB_TYPE=both -DBQ_WERROR=OFF]
    # Build for the native architecture instead of upstream's universal default
    args << "-DCMAKE_OSX_ARCHITECTURES=#{Hardware::CPU.arch}" if OS.mac?

    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    # Each tool installs another copy of the library's docs directory
    rm_r share.glob("doc/BqLog_*/docs")
  end

  test do
    (testpath/"test.cpp").write <<~'CPP'
      #include <bq_log/bq_log.h>

      int main() {
        bq::log log = bq::log::create_log("test",
          "appenders_config.file.type=compressed_file\n"
          "appenders_config.file.file_name=log/test\n"
          "appenders_config.file.levels=[all]");
        if (!log.is_valid()) return 1;
        log.info("Hello Homebrew!");
        log.force_flush();
        return 0;
      }
    CPP
    system ENV.cxx, "test.cpp", "-std=c++17", "-I#{include}", "-L#{lib}", "-lBqLog", "-Wl,-rpath,#{lib}", "-o", "test"
    system "./test"

    log_file = testpath.glob("log/test_*.logcompr").first
    assert_match "Hello Homebrew!", shell_output("#{bin}/BqLog_LogDecoder #{log_file}")
  end
end
