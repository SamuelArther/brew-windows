class RancherMachine < Formula
  desc "Machine management for a container-centric world"
  homepage "https://github.com/rancher/machine"
  url "https://github.com/rancher/machine/archive/refs/tags/v0.15.0-rancher145.tar.gz"
  version "0.15.0-rancher145"
  sha256 "e039feb0f202854a97737bf7eadfde1d1a647294494c153791d191f8277835b0"
  license "Apache-2.0"
  head "https://github.com/rancher/machine.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+-rancher\d+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "395226a306b3f0cd6bd740b9fd47d45d84c5480b65a74542d2819e63c122cbaf"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    commit = build.head? ? Utils.git_short_head : tap.user
    ldflags = %W[
      -X github.com/rancher/machine/version.Version=#{version}
      -X github.com/rancher/machine/version.GitCommit=#{commit}
    ]
    system "go", "build", *std_go_args(ldflags:), "./cmd/rancher-machine"
  end

  service do
    run [opt_bin/"rancher-machine", "start", "default"]
    environment_variables PATH: std_service_path_env
    run_type :immediate
    working_dir HOMEBREW_PREFIX
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rancher-machine --version")
    assert_match "VBoxManage --version", shell_output("#{bin}/rancher-machine create test", 3)
  end
end
