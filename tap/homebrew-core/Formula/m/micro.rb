class Micro < Formula
  desc "Modern and intuitive terminal-based text editor"
  homepage "https://github.com/micro-editor/micro"
  url "https://github.com/micro-editor/micro/releases/download/v2.0.15/micro-2.0.15-win64.zip"
  sha256 "90635c53c11aa2a0d997f5e3ed43528877740725500207640b29551cef18479b"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/micro.exe", "micro"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/micro -version")
  end
end
