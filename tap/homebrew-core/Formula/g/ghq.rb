class Ghq < Formula
  desc "Remote repository management made easy"
  homepage "https://github.com/x-motemen/ghq"
  url "https://github.com/x-motemen/ghq/releases/download/v1.11.2/ghq_windows_amd64.zip"
  sha256 "7ab0e759ac9f117f0b5eeea0985c58e693ecb8d2e29b7bcfbce657caac31bce8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/x-motemen/ghq.git"
  end

  def install
    install_tree
    windows_exe "libexec/ghq.exe", "ghq"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ghq --version 2>&1")
  end
end
