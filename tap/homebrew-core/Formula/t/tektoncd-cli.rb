class TektoncdCli < Formula
  desc "CLI for interacting with TektonCD"
  homepage "https://github.com/tektoncd/cli"
  url "https://github.com/tektoncd/cli/archive/refs/tags/v0.46.2.tar.gz"
  sha256 "63485a0d3a3c0b59408c09dc0e5515e9ae95e836e18fa92548c21eaad598d359"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "f5ae05da724e875e83ea4dc42294561636a4ebb5f5389b99c7c90e4ed6296574"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    system "make", "bin/tkn"
    bin.install "bin/tkn"

    generate_completions_from_executable(bin/"tkn", shell_parameter_format: :cobra)
  end

  test do
    output = shell_output("#{bin}/tkn pipelinerun describe homebrew-formula 2>&1", 1)
    assert_match "Error: Couldn't get kubeConfiguration namespace", output
  end
end
