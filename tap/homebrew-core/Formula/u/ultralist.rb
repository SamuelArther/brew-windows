class Ultralist < Formula
  desc "Simple GTD-style task management for the command-line"
  homepage "https://ultralist.io"
  url "https://github.com/gammons/ultralist/releases/download/1.7.0/ultralist_win.zip"
  sha256 "0e8750f348cde39b9e543471c64b1ab2ebeb0593f477c8645a8e6033eb6033a0"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gammons/ultralist/archive/refs/tags/1.7.0.tar.gz"
  end

  def install
    bin.install "ultralist.exe"
  end

  test do
    system bin/"ultralist", "init"
    assert_path_exists testpath/".todos.json"
    add_task = shell_output("#{bin}/ultralist add learn the Tango")
    assert_match(/Todo.* added/, add_task)
  end
end
