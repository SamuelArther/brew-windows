class MinijinjaCli < Formula
  desc "Render Jinja2 templates directly from the command-line to stdout"
  homepage "https://docs.rs/minijinja/latest/minijinja/"
  url "https://github.com/mitsuhiko/minijinja/releases/download/3.0.1/minijinja-cli-x86_64-pc-windows-msvc.zip"
  sha256 "fb50e280eb2356672319e092aee51f28b870e9feddc145ef56abcbeef22e5fd2"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mitsuhiko/minijinja/archive/refs/tags/3.0.1.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "minijinja-cli.exe"
  end

  test do
    (testpath/"test.jinja").write <<~JINJA
      Hello {{ name }}
    JINJA

    assert_equal "Hello Homebrew\n", shell_output("#{bin}/minijinja-cli test.jinja --define name=Homebrew")
  end
end
