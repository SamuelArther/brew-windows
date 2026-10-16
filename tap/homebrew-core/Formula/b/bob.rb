class Bob < Formula
  desc "Version manager for neovim"
  homepage "https://github.com/MordechaiHadad/bob"
  url "https://github.com/MordechaiHadad/bob/releases/download/v4.2.0/bob-windows-x86_64.zip"
  sha256 "690407ed9d5dde9644eec2b5b750b9cc8dc17da5c164c19500b196444a9b3bbc"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/MordechaiHadad/bob/archive/refs/tags/v4.2.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bob.exe", "bob"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bob --version 2>&1")
  end
end
