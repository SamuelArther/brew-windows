class Lychee < Formula
  desc "Fast, async, resource-friendly link checker"
  homepage "https://lychee.cli.rs/"
  url "https://github.com/lycheeverse/lychee/releases/download/lychee-v0.24.2/lychee-x86_64-pc-windows-msvc.zip"
  version "0.24.2"
  sha256 "32975d1493ee1a975d6bb41e4fb56fe419cb442ded628bb772ba2e614acfacad"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lycheeverse/lychee/archive/refs/tags/lychee-v0.24.2.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/lychee.exe", "lychee"
  end

  test do
    (testpath/"test.md").write "[This](https://example.com) is an example.\n"
    output = shell_output("#{bin}/lychee #{testpath}/test.md")
    assert_match "🔍 1 Total", output
    assert_match "✅ 0 OK 🚫 0 Errors 👻 1 Excluded", output
  end
end
