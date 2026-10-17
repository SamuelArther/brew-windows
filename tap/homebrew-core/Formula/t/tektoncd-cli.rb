class TektoncdCli < Formula
  desc "CLI for interacting with TektonCD"
  homepage "https://github.com/tektoncd/cli"
  url "https://github.com/tektoncd/cli/archive/refs/tags/v0.46.1.tar.gz"
  sha256 "3432cf4ea1f0014305d41d4a703f9c0b7910a1d693b32d82191e07ca35b2337d"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "711832805b456fd57ddb5a0d24d537f48df32ddec71acd48498ce139bf8cba50"
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
