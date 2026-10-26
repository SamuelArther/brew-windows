class RRig < Formula
  desc "R Installation Manager"
  homepage "https://github.com/r-lib/rig"
  url "https://github.com/r-lib/rig/archive/refs/tags/v0.11.1.tar.gz"
  sha256 "e6186c4fd550f1c31323d5732c95da78e8a2f0cafcd3e505264b2ec7f5a09da2"
  license "MIT"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "573c5150786a829c6f5db65b55a242aa479e95750ccabeb3a8b72e6d9cef293c"
  end

  depends_on "rust" => :build

  conflicts_with "rig", because: "both install `rig` binary"

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rig --version")
    output = shell_output("#{bin}/rig default 2>&1", 1)
    assert_match "No default R version is set", output
  end
end
