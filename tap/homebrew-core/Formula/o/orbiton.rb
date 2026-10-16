class Orbiton < Formula
  desc "Fast and config-free text editor and IDE limited by VT100"
  homepage "https://roboticoverlords.org/orbiton/"
  url "https://github.com/xyproto/orbiton/releases/download/v2.74.6/orbiton-2.74.6-windows_x86_64_static.zip"
  sha256 "16804866c4be7661da9f4f238d92cec8555bc632118cf85cfee10d7c9f0c900b"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/xyproto/orbiton/archive/refs/tags/v2.74.6.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/o.exe", "o"
  end

  test do
    (testpath/"hello.txt").write "hello\n"
    copy_command = "#{bin}/o --copy #{testpath}/hello.txt"
    paste_command = "#{bin}/o --paste #{testpath}/hello2.txt"

    if OS.linux?
      system "xvfb-run", "sh", "-c", "#{copy_command} && #{paste_command}"
      assert_equal (testpath/"hello.txt").read, (testpath/"hello2.txt").read
    else
      # `--copy` and `--paste` need the pasteboard, which the test sandbox blocks
      assert_match "hello", shell_output("#{bin}/o --list #{testpath}/hello.txt")
    end
  end
end
