class Python3 < Formula
  desc "Interpreted, interactive, object-oriented programming language"
  homepage "https://www.python.org/"
  url "https://api.nuget.org/v3-flatcontainer/python/3.15.0/python.3.15.0.nupkg"
  sha256 "2f59399a704f1b7078606eca622992021fa4521058e25fdd09e55f8533f2a12e"
  license "Python-2.0"

  # Official full CPython NuGet tools distribution, not the restricted embeddable ZIP.

  def install
    install_tree
    windows_exe "libexec/tools/python.exe", "python"
    windows_exe "libexec/tools/python.exe", "python3"
    windows_exe "libexec/tools/python.exe", "pip3", "-m", "pip"
  end
end
