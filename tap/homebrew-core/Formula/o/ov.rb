class Ov < Formula
  desc "Feature-rich terminal-based text viewer"
  homepage "https://noborus.github.io/ov/"
  url "https://github.com/noborus/ov/releases/download/v0.55.0/ov_0.55.0_windows_amd64.zip"
  sha256 "505dec78254094f8a5612b8fa7e6a6a42bdf4dc37c42a0d4dbd16bd62a57a8a4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/noborus/ov/archive/refs/tags/v0.55.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/ov.exe", "ov"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ov --version")

    (testpath/"test.txt").write("Hello, world!")
    assert_match "Hello, world!", shell_output("#{bin}/ov test.txt")
  end
end
