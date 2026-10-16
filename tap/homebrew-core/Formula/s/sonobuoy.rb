class Sonobuoy < Formula
  desc "Kubernetes component that generates reports on cluster conformance"
  homepage "https://sonobuoy.io/"
  url "https://github.com/vmware-tanzu/sonobuoy/archive/refs/tags/v0.57.5.tar.gz"
  sha256 "77eab0b152bbaf9836885630b92fa7ab3ca1e561b8db567d9401305a21f166a3"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "388155bb2bf6d64d419f87ba97e48baba4e2d948c0dc78f1579ffa5e1be2a5c8"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/vmware-tanzu/sonobuoy/pkg/buildinfo.Version=v#{version}"
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"sonobuoy", shell_parameter_format: :cobra)
  end

  test do
    assert_match "Sonobuoy is a Kubernetes component that generates reports on cluster conformance",
      shell_output("#{bin}/sonobuoy 2>&1")
    assert_match version.to_s,
      shell_output("#{bin}/sonobuoy version 2>&1")
    assert_match "name: sonobuoy",
      shell_output("#{bin}/sonobuoy gen --kubernetes-version=v1.21 2>&1")
  end
end
