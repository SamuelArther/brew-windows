class Kubecm < Formula
  desc "KubeConfig Manager"
  homepage "https://kubecm.cloud"
  url "https://github.com/sunny0826/kubecm/archive/refs/tags/v0.36.0.tar.gz"
  sha256 "ae03e76d4401c1d705a4a2684d5ba2eef8e8eb47a30df5bb80abfb65ba19f34a"
  license "Apache-2.0"
  head "https://github.com/sunny0826/kubecm.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a871c2af0487c0dc93f5319c037b9c74b30564ae9e14d8a35884ece4de05a1fc"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/sunny0826/kubecm/version.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"kubecm", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kubecm version")
    # Should error out as switch context need kubeconfig
    assert_match "Error: open", shell_output("#{bin}/kubecm switch 2>&1", 1)
  end
end
