class Draft < Formula
  desc "Day 0 tool for getting your app on Kubernetes fast"
  homepage "https://github.com/Azure/draft"
  url "https://github.com/Azure/draft/archive/refs/tags/v0.17.16.tar.gz"
  sha256 "ba8eaba15bf731e1127747a02a2d899a1f1f1632c3acdf0b51f4268866284d1c"
  license "MIT"
  head "https://github.com/Azure/draft.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "563cc6376222bcd0eed943354d3e84eff06448720e26485cda4b4daf0cfa689b"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/Azure/draft/cmd.VERSION=#{version}"
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"draft", shell_parameter_format: :cobra)
  end

  test do
    supported_deployment_types = JSON.parse(shell_output("#{bin}/draft info"))["supportedDeploymentTypes"]
    assert_equal ["helm", "kustomize", "manifests"], supported_deployment_types

    assert_match version.to_s, shell_output("#{bin}/draft version")
  end
end
