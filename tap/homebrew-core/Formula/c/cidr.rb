class Cidr < Formula
  desc "CLI to perform various actions on CIDR ranges"
  homepage "https://github.com/bschaatsbergen/cidr"
  url "https://github.com/bschaatsbergen/cidr/releases/download/v2.3.0/cidr_2.3.0_windows_amd64.tar.gz"
  sha256 "925de0f2c11c078263cebf7d223a6fab7b7571a67c2901dc02aaa8df377bc3bb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bschaatsbergen/cidr/archive/refs/tags/v2.3.0.tar.gz"
  end

  def install
    bin.install "cidr.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cidr --version")
    assert_equal "65536\n", shell_output("#{bin}/cidr count 10.0.0.0/16")
    assert_equal "1\n", shell_output("#{bin}/cidr count 10.0.0.0/32")
    assert_equal "false\n", shell_output("#{bin}/cidr overlaps 10.106.147.0/24 10.106.149.0/23")
  end
end
