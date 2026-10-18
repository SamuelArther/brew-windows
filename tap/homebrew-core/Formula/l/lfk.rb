class Lfk < Formula
  desc "Terminal user interface for navigating and managing Kubernetes clusters"
  homepage "https://github.com/janosmiko/lfk"
  url "https://github.com/janosmiko/lfk/archive/refs/tags/v0.19.7.tar.gz"
  sha256 "4a854f7b380696ada35b538d9207a529cfb23e5b1fc87d0282979fc7fbbb745e"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c3faf2e5452492a16c17232b4be5d56e1e665c3c081b969e079fdca6fec097c4"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "0"
    ldflags = %W[
      -X github.com/janosmiko/lfk/internal/version.Version=#{version}
      -X github.com/janosmiko/lfk/internal/version.BuildDate=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    # This program is TUI-only
    assert_match version.to_s, shell_output("#{bin}/lfk version")
  end
end
