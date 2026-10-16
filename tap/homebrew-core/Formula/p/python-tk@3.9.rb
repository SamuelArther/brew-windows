class PythonTkAT39 < Formula
  desc "Python interface to Tcl/Tk"
  homepage "https://www.python.org/"
  url "https://www.python.org/ftp/python/3.9.25/Python-3.9.25.tar.xz"
  sha256 "00e07d7c0f2f0cc002432d1ee84d2a40dae404a99303e3f97701c10966c91834"
  license "Python-2.0"

  # Follow up to python@3.9 deprecation
  deprecate! date: "2025-10-15", because: :deprecated_upstream
  disable! date: "2026-10-15", because: :deprecated_upstream

  depends_on "python@3.9"
  depends_on "tcl-tk@8"

  deny_network_access!

  def install
    cd "Modules" do
      tcltk = Formula["tcl-tk@8"]
      tcltk_version = tcltk.any_installed_version.major_minor
      Pathname("setup.py").write <<~PYTHON
        from setuptools import setup, Extension

        setup(name="tkinter",
              description="#{desc}",
              version="#{version}",
              ext_modules = [
                Extension("_tkinter", ["_tkinter.c", "tkappinit.c"],
                          define_macros=[("WITH_APPINIT", 1)],
                          include_dirs=["#{tcltk.opt_include/"tcl-tk"}"],
                          libraries=["tcl#{tcltk_version}", "tk#{tcltk_version}"],
                          library_dirs=["#{tcltk.opt_lib}"])
              ]
        )
      PYTHON
      args = std_pip_args(prefix: false, build_isolation: false).reject { |s| s["--uploaded-prior-to"] }
      system python3, "-m", "pip", "install", *args, "--target=#{libexec}", "."
      rm_r libexec.glob("*.dist-info")
    end
  end

  test do
    system python3, "-c", "import tkinter"
  end
end
