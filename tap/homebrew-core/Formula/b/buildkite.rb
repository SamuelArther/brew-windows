class Buildkite < Formula
  desc "Work with Buildkite from the command-line"
  homepage "https://github.com/buildkite/cli"
  url "https://github.com/buildkite/cli/archive/refs/tags/v3.65.0.tar.gz"
  sha256 "df4b7cf62080f240834d4243cfc09c8f638c00da658527f7053c61bb487491b2"
  license "MIT"
  head "https://github.com/buildkite/cli.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d0da0dec9cd74781797a839a4d2439c69ab82dd39bf4bdd9f30435ba32b82beb"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-s -w -X github.com/buildkite/cli/v3/cmd/version.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "."
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/buildkite --version")

    system bin/"buildkite", "config", "set", "output_format", "yaml", "--local"
    assert_equal "yaml", shell_output("#{bin}/buildkite config get output_format").strip
  end
end
