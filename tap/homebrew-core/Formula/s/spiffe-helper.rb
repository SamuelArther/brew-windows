class SpiffeHelper < Formula
  desc "Tool that can be used to retrieve and manage SVIDs on behalf of a workload"
  homepage "https://github.com/spiffe/spiffe-helper"
  url "https://github.com/spiffe/spiffe-helper/archive/refs/tags/v0.12.2.tar.gz"
  sha256 "ccfcce0ae20ab613fb73048189931f52deb996f999561cb1f1681f65fb0705cb"
  license "Apache-2.0"
  head "https://github.com/spiffe/spiffe-helper.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b092ac09953d480f205da6601e96d2bd6da81e79f2c9966fb2aa0b6867219587"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/spiffe/spiffe-helper/pkg/version.gittag=#{version}"
    system "go", "build", *std_go_args(ldflags:), "./cmd/spiffe-helper"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/spiffe-helper -version")

    output = shell_output("#{bin}/spiffe-helper 2>&1", 1)
    # Windows words ENOENT "The system cannot find the file specified".
    assert_match "helper.conf: #{OS.windows? ? "The system cannot find the file specified" : "no such file or directory"}", output
  end
end
