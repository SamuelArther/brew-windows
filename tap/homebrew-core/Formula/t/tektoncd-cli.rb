class TektoncdCli < Formula
  desc "CLI for interacting with TektonCD"
  homepage "https://github.com/tektoncd/cli"
  url "https://github.com/tektoncd/cli/archive/refs/tags/v0.47.0.tar.gz"
  sha256 "a0f2b0f0ca69504a57923c2048f799c8198c6e372566a8b90eb1af81384cbab9"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "81324fa2fdd8269580589e7a05248d35fead68c639328c85e4be4a039ac99d7a"
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
