class Kubespy < Formula
  desc "Tools for observing Kubernetes resources in realtime"
  homepage "https://github.com/pulumi/kubespy"
  url "https://github.com/pulumi/kubespy/archive/refs/tags/v0.6.3.tar.gz"
  sha256 "1975bf0a0aeb03e69c42ac626c16cd404610226cc5f50fab96d611d9eb6a6d29"
  license "Apache-2.0"
  head "https://github.com/pulumi/kubespy.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "32f2064e191e3baeabbf145f9b891f8740b885c97307f947365a8e22bff7ec3a"
  end

  depends_on "go" => :build

  def install
    system "go", "build", *std_go_args(ldflags: "-X github.com/pulumi/kubespy/version.Version=#{version}")

    generate_completions_from_executable(bin/"kubespy", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kubespy version")

    assert_match "invalid configuration: no configuration has been provided",
                 shell_output("#{bin}/kubespy status v1 Pod nginx 2>&1", 1)
  end
end
