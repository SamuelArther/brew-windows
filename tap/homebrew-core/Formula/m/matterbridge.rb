class Matterbridge < Formula
  desc "Protocol bridge for multiple chat platforms"
  homepage "https://github.com/42wim/matterbridge"
  url "https://github.com/42wim/matterbridge/archive/refs/tags/v1.26.0.tar.gz"
  sha256 "00e1bbfe3b32f2feccf9a7f13a6f12b1ce28a5eb04cc7b922b344e3493497425"
  license "Apache-2.0"
  head "https://github.com/42wim/matterbridge.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1276c79b4e1ad753c6a40b954481953f6441fbee841721830fe0a9a161e5b642"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    touch testpath/"test.toml"
    assert_match "no [[gateway]] configured", shell_output("#{bin}/matterbridge -conf test.toml 2>&1", 1)
  end
end
