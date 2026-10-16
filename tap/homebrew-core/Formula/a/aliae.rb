class Aliae < Formula
  desc "Cross shell and platform alias management"
  homepage "https://aliae.dev"
  url "https://github.com/JanDeDobbeleer/aliae/releases/download/v1.6.0/aliae-windows-amd64.exe"
  sha256 "542f56c9db862063de2718b5e378047124c14099dee6c4a333e7a1ce857a489e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jandedobbeleer/aliae/archive/refs/tags/v1.6.0.tar.gz"
  end

  def install
    bin.install "aliae-windows-amd64.exe" => "aliae.exe"
  end

  test do
    (testpath/".aliae.yaml").write <<~YAML
      alias:
        - name: a
          value: aliae
        - name: hello-world
          value: echo "hello world"
          type: function
    YAML

    output = shell_output("#{bin}/aliae init bash")
    assert_equal <<~SHELL.chomp, output
      alias a="aliae"
      hello-world() {
          echo "hello world"
      }
    SHELL

    assert_match version.to_s, shell_output("#{bin}/aliae --version")
  end
end
