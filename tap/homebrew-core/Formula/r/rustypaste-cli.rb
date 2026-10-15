class RustypasteCli < Formula
  desc "CLI tool for rustypaste"
  homepage "https://blog.orhun.dev/blazingly-fast-file-sharing"
  url "https://github.com/orhun/rustypaste-cli/releases/download/v0.10.0/rustypaste-cli-0.10.0-x86_64-pc-windows-msvc.zip"
  sha256 "e104cb393f7bb61ab2fa0a7b2dc7222f47dc9b3cfa14d61aa2d302f01e8c08cd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/orhun/rustypaste-cli/archive/refs/tags/v0.10.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/rpaste.exe", "rpaste"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rpaste --version 2>&1")
  end
end
