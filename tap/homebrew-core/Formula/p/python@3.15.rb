class PythonAT315 < Formula
  desc "Interpreted, interactive, object-oriented programming language"
  homepage "https://www.python.org/"
  url "https://api.nuget.org/v3-flatcontainer/python/3.15.0/python.3.15.0.nupkg"
  sha256 "2f59399a704f1b7078606eca622992021fa4521058e25fdd09e55f8533f2a12e"
  license "Python-2.0"
  revision 1

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
    # Upstream's libexec/bin/python and python3, which multi-Python formulae run as
    # `f.opt_libexec/"bin/python"` (numpy, rpds-py, py3cairo). Python finds its home through the link.
    (libexec/"bin").install_symlink libexec/"tools/python.exe"
    (libexec/"bin").install_symlink libexec/"tools/python.exe" => "python3.exe"
    windows_exe "libexec/tools/python.exe", "python"
    windows_exe "libexec/tools/python.exe", "python3"
    windows_exe "libexec/tools/python.exe", "pip3", "-m", "pip"
    windows_exe "libexec/tools/python.exe", "python3.15"
    windows_exe "libexec/tools/python.exe", "pip3.15", "-m", "pip"
  end

  # Like upstream's sitecustomize: packages linked into HOMEBREW_PREFIX/lib/python3.15/site-packages
  # are importable. While formulae build (HOMEBREW_PYTHON_PREFIX_SCHEME=posix), `pip install --prefix`
  # uses upstream's lib/python3.15/site-packages + bin layout instead of Lib\site-packages + Scripts.
  def post_install
    site_packages = HOMEBREW_PREFIX/"lib/python3.15/site-packages"
    scheme = "{'stdlib': '{installed_base}/Lib', 'platstdlib': '{base}/Lib', " \
             "'purelib': '{base}/lib/python{py_version_short}/site-packages', " \
             "'platlib': '{base}/lib/python{py_version_short}/site-packages', " \
             "'include': '{installed_base}/Include', 'platinclude': '{installed_base}/Include', " \
             "'scripts': '{base}/bin', 'data': '{base}'}"
    (libexec/"tools/Lib/site-packages/homebrew.pth").write <<~PTH
      import site; site.addsitedir(r'#{site_packages}')
      import os, sys, sysconfig; os.environ.get('HOMEBREW_PYTHON_PREFIX_SCHEME') == 'posix' and sys.prefix == sys.base_prefix and (sysconfig._INSTALL_SCHEMES.__setitem__('homebrew_prefix', #{scheme}), setattr(sysconfig, 'get_preferred_scheme', (lambda f: lambda key: 'homebrew_prefix' if key == 'prefix' else f(key))(sysconfig.get_preferred_scheme)))
    PTH
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
