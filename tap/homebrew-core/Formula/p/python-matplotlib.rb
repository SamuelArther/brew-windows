class PythonMatplotlib < Formula
  desc "Python library for creating static, animated, and interactive visualizations"
  homepage "https://matplotlib.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-matplotlib-3.11.2-1-any.pkg.tar.zst"
  sha256 "8c4a3f4feeca92fa8d79becc90de8e84947a0c581e55cd5b0491cef5715d2d8a"
  license "PSF-2.0"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://files.pythonhosted.org/packages/e7/c8/9aa712a0afb882649424dd8de8ad9aa6235e796e84c6052e8f6dc1598d0d/matplotlib-3.11.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"python3.14/site-packages/__pycache__/pylab.cpython-314.opt-1.pyc"
  end
end
