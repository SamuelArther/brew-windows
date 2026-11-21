class FleetCli < Formula
  desc "Manage large fleets of Kubernetes clusters"
  homepage "https://fleet.rancher.io/"
  url "https://github.com/rancher/fleet/archive/refs/tags/v0.17.0.tar.gz"
  sha256 "584631ac15d92f89f4791bbd47306a16812e89ae6901b069a6a0a1151812931a"
  license "Apache-2.0"
  head "https://github.com/rancher/fleet.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "69c2ff25f89a8b9eee680659ea358006c532a6f7861cce96d9d3303323aecce3"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/rancher/fleet/pkg/version.Version=#{version}
      -X github.com/rancher/fleet/pkg/version.GitCommit=#{tap.user}
    ]
    system "go", "build", *std_go_args(output: bin/"fleet", ldflags:), "./cmd/fleetcli"

    generate_completions_from_executable(bin/"fleet", shell_parameter_format: :cobra)
  end

  test do
    system "git", "clone", "https://github.com/rancher/fleet-examples"
    assert_match "kind: Deployment", shell_output("#{bin}/fleet test fleet-examples/simple 2>&1")

    assert_match version.to_s, shell_output("#{bin}/fleet --version")
  end
end
