class Buf < Formula
  desc "New way of working with Protocol Buffers"
  homepage "https://buf.build"
  url "https://github.com/bufbuild/buf/releases/download/v1.74.0/buf-Windows-x86_64.zip"
  sha256 "b0e26af2a4bef62724f6a58114a6027e3a19bdf98033b2107995bc243d7c8c97"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/buf.exe", "buf"
    windows_exe "libexec/bin/protoc-gen-buf-breaking.exe", "protoc-gen-buf-breaking"
    windows_exe "libexec/bin/protoc-gen-buf-lint.exe", "protoc-gen-buf-lint"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/buf --version")
  end
end
