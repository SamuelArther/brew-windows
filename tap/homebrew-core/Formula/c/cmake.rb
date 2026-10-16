class Cmake < Formula
  desc "Cross-platform make"
  homepage "https://www.cmake.org/"
  url "https://github.com/Kitware/CMake/releases/download/v4.4.4/cmake-4.4.4-windows-x86_64.zip"
  sha256 "bace36e94b31c68ab6fa295f26dfa11219e0701cf7c94b0284a7d1cb13dac536"
  license "BSD-3-Clause"

  livecheck do
    url "https://cmake.org/download/"
    regex(/href=.*?cmake[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/cmake.exe", "cmake"
    windows_exe "libexec/bin/ctest.exe", "ctest"
    windows_exe "libexec/bin/cpack.exe", "cpack"
  end

  test do
    # Configuring a project needs a build tool (Visual Studio, NMake, Ninja or make) that a
    # stock Windows install doesn't have, so run CMake in script mode instead.
    (testpath/"test.cmake").write <<~CMAKE
      cmake_minimum_required(VERSION #{version.major_minor})
      find_package(Git)
      message(STATUS "CMake ${CMAKE_VERSION} works")
    CMAKE
    assert_match "CMake #{version} works", shell_output("#{bin}/cmake -P test.cmake")

    # These should be supplied in a separate cmake-docs formula.
    refute_path_exists doc/"html"
    refute_path_exists man
  end
end
