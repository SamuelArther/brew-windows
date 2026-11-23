class Velero < Formula
  desc "Disaster recovery for Kubernetes resources and persistent volumes"
  homepage "https://velero.io/"
  url "https://github.com/velero-io/velero/archive/refs/tags/v1.19.0.tar.gz"
  sha256 "789f13190b0d35a8edadddfb7dfb442236f2e2699c6a78b1d3fd45da7c7957c1"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "71939f83b18dee34dea2085b92930cb4bce4ff61f792a80a6393148ee7c46d31"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    commit = build.head? ? Utils.git_head : Utils::Git.get_tar_commit_id(cached_download)
    ldflags = %W[
      -X github.com/velero-io/velero/pkg/buildinfo.Version=#{version}
      -X github.com/velero-io/velero/pkg/buildinfo.GitSHA=#{commit}
      -X github.com/velero-io/velero/pkg/buildinfo.GitTreeState=clean
    ]
    system "go", "build", *std_go_args(ldflags:), "-installsuffix", "static", "./cmd/velero"

    generate_completions_from_executable(bin/"velero", "completion")
  end

  test do
    output = shell_output("#{bin}/velero 2>&1")
    assert_match "Velero is a tool for managing disaster recovery", output
    assert_match "Version: #{version}", shell_output("#{bin}/velero version --client-only 2>&1")
    system bin/"velero", "client", "config", "set", "TEST=value"
    assert_match "value", shell_output("#{bin}/velero client config get 2>&1")
  end
end
