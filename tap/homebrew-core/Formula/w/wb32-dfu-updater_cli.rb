class Wb32DfuUpdaterCli < Formula
  desc "USB programmer for downloading and uploading firmware to/from USB devices"
  homepage "https://github.com/WestberryTech/wb32-dfu-updater"
  url "https://github.com/WestberryTech/wb32-dfu-updater/archive/refs/tags/1.0.0.tar.gz"
  sha256 "2b1c5b5627723067168af9740cb25c5c179634e133e4ced06028462096de5699"
  license "Apache-2.0"
  head "https://github.com/WestberryTech/wb32-dfu-updater.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7ec8a709301f5864e7689c27c572fdcf74de5e18855e49e938b26dfb3e19b33b"
  end

  depends_on "cmake" => :build
  depends_on "libusb"

  # Fix compatibility with cmake 4+
  patch do
    url "https://github.com/WestberryTech/wb32-dfu-updater/commit/34725776e4b21be89e5d4eb0ea83346f26fc5d1f.patch?full_index=1"
    sha256 "bfcc362f17c3063b90531e2795e7d33743dc754cce5dac2a93582994f9a88479"
    type :backport
    resolves "https://github.com/WestberryTech/wb32-dfu-updater/pull/17"
  end

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    assert_match "No DFU capable USB device available\n",
                 shell_output("#{bin}/wb32-dfu-updater_cli -U 111.bin 2>&1", 74)
  end
end
