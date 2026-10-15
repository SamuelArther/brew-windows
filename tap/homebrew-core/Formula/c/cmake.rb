class Cmake < Formula
  desc "Cross-platform build system"
  homepage "https://cmake.org/"
  url "https://github.com/Kitware/CMake/releases/download/v4.4.4/cmake-4.4.4-windows-x86_64.zip"
  sha256 "bace36e94b31c68ab6fa295f26dfa11219e0701cf7c94b0284a7d1cb13dac536"
  license "BSD-3-Clause"

  def install
    install_tree
    windows_exe "libexec/bin/cmake.exe", "cmake"
    windows_exe "libexec/bin/ctest.exe", "ctest"
    windows_exe "libexec/bin/cpack.exe", "cpack"
  end
end
