class ConsoleBridge < Formula
  desc "Robot Operating System-independent package for logging"
  homepage "https://wiki.ros.org/console_bridge/"
  url "https://github.com/ros/console_bridge/archive/refs/tags/1.0.2.tar.gz"
  sha256 "303a619c01a9e14a3c82eb9762b8a428ef5311a6d46353872ab9a904358be4a4"
  license "BSD-3-Clause"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "2ba7a6894a5473ed74356c2e0626faa779292bdab089a56dc94a01a6709cadc2"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :test

  # Fix build with CMake 4.0+. Remove on next release.
  patch do
    url "https://github.com/ros/console_bridge/commit/81ec67f6daf3cd19ef506e00f02efb1645597b9c.patch?full_index=1"
    sha256 "b2746b536b72e391c1a37363a1d8e2203d50229057bf0767f3ceae8e57784a16"
    type :backport
    resolves "https://github.com/ros/console_bridge/issues/100"
  end

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <console_bridge/console.h>

      int main() {
        CONSOLE_BRIDGE_logDebug("Testing Log");
        return 0;
      }
    CPP

    flags = shell_output("pkgconf --cflags --libs console_bridge").chomp.split
    system ENV.cxx, "test.cpp", "-o", "test", *flags
    system "./test"
  end
end
