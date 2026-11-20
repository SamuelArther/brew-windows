class Draft < Formula
  desc "Day 0 tool for getting your app on Kubernetes fast"
  homepage "https://github.com/Azure/draft"
  url "https://github.com/Azure/draft/archive/refs/tags/v0.17.18.tar.gz"
  sha256 "3dcec7ae0e5abf39cde7623fbf2ab392f02b08107516f22bc669b6ba3e8031ee"
  license "MIT"
  head "https://github.com/Azure/draft.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "55ba28f3813b86bb082f132d9b51ebf893d223f812ff580c97eb551eed8b7312"
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
