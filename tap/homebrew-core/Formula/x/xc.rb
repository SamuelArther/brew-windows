class Xc < Formula
  desc "Markdown defined task runner"
  homepage "https://xcfile.dev/"
  url "https://github.com/joerdav/xc/releases/download/v0.9.0/xc_0.9.0_windows_amd64.tar.gz"
  sha256 "34d1ced6cc23cb2ed3b70d08c6c40097fd763fcbc4b228c145357706c792896b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/joerdav/xc/archive/refs/tags/v0.9.0.tar.gz"
  end

  def install
    bin.install "xc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/xc --version")

    (testpath/"README.md").write <<~MARKDOWN
      # Tasks

      ## hello
      ```sh
      echo "Hello, world!"
      ```
    MARKDOWN

    output = shell_output("#{bin}/xc hello")
    assert_match "Hello, world!", output
  end
end
