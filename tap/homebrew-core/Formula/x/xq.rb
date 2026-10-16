class Xq < Formula
  desc "Command-line XML and HTML beautifier and content extractor"
  homepage "https://github.com/sibprogrammer/xq"
  url "https://github.com/sibprogrammer/xq/releases/download/v1.5.1/xq_1.5.1_windows_amd64.zip"
  sha256 "52ba9baba2bb74ea7711232419b2185cd046c8482a8e31c7f3cf942db4f94121"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sibprogrammer/xq/archive/refs/tags/v1.5.1.tar.gz"
  end

  def install
    bin.install "xq.exe"
  end

  test do
    version_output = shell_output("#{bin}/xq --version 2>&1")
    assert_match "xq version #{version}", version_output

    run_output = pipe_output(bin/"xq", "<root></root>")
    assert_match("<root/>", run_output)
  end
end
