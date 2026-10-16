class Valgrind < Formula
  desc "Dynamic memory checker (Dr. Memory, the Windows counterpart of Valgrind's memcheck)"
  homepage "https://drmemory.org/"
  url "https://github.com/DynamoRIO/drmemory/releases/download/cronbuild-2.6.20434/DrMemory-Windows-2.6.20434.zip"
  version "2.6.20434"
  sha256 "ed9c0e3f1bdb7f8db1adc13531493fb1c451e15f375638592c01d8837825a73a"
  license "LGPL-2.1-only"

  # Valgrind does not run on Windows. Dr. Memory (DynamoRIO project, official Windows build)
  # finds the same classes of bugs: unaddressable and uninitialized accesses, invalid and
  # double frees, and leaks. Its command line puts `--` before the program:
  #   valgrind -- program.exe args      (same as: drmemory -- program.exe args)
  # drstrace (system call tracer) and drltrace (library call tracer) ship with it.

  livecheck do
    url :stable
    regex(/^cronbuild[._-]v?(\d+(?:\.\d+)+)$/i)
    strategy :github_latest
  end

  def caveats
    <<~EOS
      valgrind runs Dr. Memory, whose options differ from Valgrind's; put `--` before the program:
        valgrind -- program.exe args
      Run `drmemory -help` for the option list. Reports are written under the logs directory and
      summarized on stderr.
    EOS
  end

  def install
    install_tree
    %w[valgrind drmemory].each { |cmd| windows_exe "libexec/bin64/drmemory.exe", cmd }
    windows_exe "libexec/bin64/drstrace.exe", "drstrace"
    windows_exe "libexec/bin64/drltrace.exe", "drltrace"
    windows_exe "libexec/bin64/symquery.exe", "symquery"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/valgrind -version")
    assert_match(/usage/i, shell_output("#{bin}/valgrind --help 2>&1"))
    assert_match "drmemory [options] -- <app", shell_output("#{bin}/drmemory -help 2>&1")
  end
end
