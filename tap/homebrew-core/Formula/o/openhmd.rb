class Openhmd < Formula
  desc "Free and open source API and drivers for immersive technology"
  homepage "http://openhmd.net"
  url "https://github.com/OpenHMD/OpenHMD/releases/download/0.3.0/OpenHMD-0.3.0-msvc2015.zip"
  sha256 "5b953eedcf1d8266c120c3091f4c17bd8a3e26c1d49ec2cdac7fc444326007c5"
  license "BSL-1.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/OpenHMD/OpenHMD/archive/refs/tags/0.3.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/openhmd_simple_example.exe", "openhmd_simple_example"
  end

  test do
    assert_path_exists bin/"openhmd_simple_example.exe"
  end
end
