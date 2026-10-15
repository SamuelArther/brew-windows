class Python3 < Formula
  desc "Interpreted, interactive, object-oriented programming language"
  homepage "https://www.python.org/"
  url "https://api.nuget.org/v3-flatcontainer/python/3.15.0/python.3.15.0.nupkg"
  sha256 "2f59399a704f1b7078606eca622992021fa4521058e25fdd09e55f8533f2a12e"
  license "Python-2.0"

  # Track the official CPython NuGet package (stable versions only).
  livecheck do
    url "https://api.nuget.org/v3-flatcontainer/python/index.json"
    strategy :json do |json|
      json["versions"]&.select { |v| v.match?(/^\d+(?:\.\d+)+$/) }
    end
  end

  # Official full CPython NuGet tools distribution, not the restricted embeddable ZIP.

  def install
    install_tree
    windows_exe "libexec/tools/python.exe", "python"
    windows_exe "libexec/tools/python.exe", "python3"
    windows_exe "libexec/tools/python.exe", "pip3", "-m", "pip"
  end

  test do
    python3 = bin/"python3"

    system python3, "-c", "import sqlite3"

    # check to see if we can create a venv
    system python3, "-m", "venv", testpath/"myvenv"

    # Check if some other modules import. Then the linked libs are working.
    system python3, "-c", "import _ctypes"
    system python3, "-c", "import _decimal"
    system python3, "-c", "import pyexpat"
    system python3, "-c", "import zlib"
    system python3, "-c", "import _zstd"

    # The NuGet distribution has no Tcl/Tk, and gdbm doesn't exist on Windows.
    assert_match "ModuleNotFoundError", shell_output("#{python3} -Sc \"import tkinter\" 2>&1", 1)
    assert_match "ModuleNotFoundError", shell_output("#{python3} -Sc \"import _gdbm\" 2>&1", 1)

    # Windows has no ndbm either; check the pure-Python DBM fallback.
    (testpath/"dbm_test.py").write <<~PYTHON
      import dbm.dumb

      with dbm.dumb.open("test", "c") as db:
          db[b"foo \\xbd"] = b"bar \\xbd"
      with dbm.dumb.open("test", "r") as db:
          assert list(db.keys()) == [b"foo \\xbd"]
          assert b"foo \\xbd" in db
          assert db[b"foo \\xbd"] == b"bar \\xbd"
    PYTHON
    system python3, "dbm_test.py"

    system bin/"pip3", "list", "--format=columns"
  end
end
