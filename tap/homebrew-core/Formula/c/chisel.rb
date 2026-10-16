class Chisel < Formula
  desc "Collection of LLDB commands to assist debugging iOS apps"
  homepage "https://github.com/facebook/chisel"
  url "https://github.com/facebook/chisel/archive/refs/tags/2.0.1.tar.gz"
  sha256 "6f019d5e7ab5eb06542a9eccbbe29e7d26165d3676828a32e143575ff102d5f9"
  license "MIT"
  head "https://github.com/facebook/chisel.git", branch: "main"

  # Chisel's commands are Python scripts loaded by LLDB's script bridge, which LLVM's Windows LLDB
  # has too, so they install exactly as upstream does. The optional Chisel.framework helper is an
  # iOS-simulator framework that only Xcode can build, so it is left out (the commands that use it
  # load it into the debugged app only when it is present).

  def install
    libexec.install Dir["*.py", "commands"]
  end

  def caveats
    <<~EOS
      Add the following line to ~/.lldbinit (%USERPROFILE%\\.lldbinit) to load chisel in LLDB:
        command script import #{opt_libexec}/fbchisellldb.py
    EOS
  end

  test do
    # No LLDB with a matching Python on the runner; check that the entry point and every command
    # module are in place.
    assert_match "def __lldb_init_module", (libexec/"fbchisellldb.py").read
    assert_path_exists libexec/"fblldb.py"
    assert_path_exists libexec/"commands/FBPrintCommands.py"
    assert_path_exists libexec/"commands/FBFindCommands.py"
  end
end
