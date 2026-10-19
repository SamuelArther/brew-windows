class Numpy < Formula
  desc "Package for scientific computing with Python"
  homepage "https://www.numpy.org/"
  url "https://files.pythonhosted.org/packages/95/b0/c7453d0b6e2073c3264468b106ee1563750cecc910965e67357e3698c83e/numpy-2.5.4.tar.gz"
  sha256 "9a94cf751c9ad8ebaa835bcd3d40dacf8534ad086b88c38029b65123c7999d2a"
  license "BSD-3-Clause"
  compatibility_version 1
  head "https://github.com/numpy/numpy.git", branch: "main"

  depends_on "gcc" => :build # for gfortran
  depends_on "meson" => :build
  depends_on "ninja" => :build
  depends_on "python@3.14" => [:build, :test]
  depends_on "python@3.15" => [:build, :test]
  depends_on "openblas"

  on_linux do
    depends_on "patchelf" => :build
  end

  def pythons
    deps.map(&:to_formula)
        .select { |f| f.name.start_with?("python@") }
        .sort_by(&:version) # so scripts like `bin/f2py` use newest python
  end

  def install
    pythons.each do |python|
      python3 = python.opt_libexec/"bin/python"
      system python3, "-m", "pip", "install", "-Csetup-args=-Dblas=openblas",
                                              "-Csetup-args=-Dlapack=openblas",
                                              *std_pip_args(build_isolation: true), "."
    end
  end

  def caveats
    <<~EOS
      To run `f2py`, you may need to `brew install #{pythons.last}`
    EOS
  end

  test do
    # TEMP diagnostic (revert): which DLL _multiarray_umath can't load.
    diag = <<~PYTHON
      import ctypes, glob, os, struct
      d = r"#{HOMEBREW_PREFIX}/lib/python3.14/site-packages/numpy/_core"
      k = r"#{prefix}/lib/python3.14/site-packages/numpy/_core"
      def imports(path):
          data = open(path, "rb").read()
          pe = struct.unpack_from("<I", data, 0x3c)[0]
          nsec, optsz = struct.unpack_from("<H", data, pe + 6)[0], struct.unpack_from("<H", data, pe + 20)[0]
          opt = pe + 24
          ddir = opt + (112 if struct.unpack_from("<H", data, opt)[0] == 0x20b else 96)
          rva = struct.unpack_from("<I", data, ddir + 8)[0]
          secs = [struct.unpack_from("<8sIIII", data, opt + optsz + i * 40) for i in range(nsec)]
          def off(r):
              for n, vs, va, rs, rp in secs:
                  if va <= r < va + max(vs, rs): return r - va + rp
          o, names = off(rva), []
          while o is not None:
              nr = struct.unpack_from("<I", data, o + 12)[0]
              if nr == 0: break
              no = off(nr); names.append(data[no:data.index(b"\\0", no)].decode()); o += 20
          return names
      sysdir = os.path.join(os.environ["SystemRoot"], "System32")
      for f in glob.glob(k + "/*.dll") + glob.glob(k + "/_multiarray_umath*.pyd"):
          missing = [n for n in imports(f) if not os.path.exists(os.path.join(k, n)) and not os.path.exists(os.path.join(sysdir, n)) and not n.lower().startswith("api-ms-")]
          print("IMPORTS", os.path.basename(f), imports(f), "MISSING", missing)
      for base in (d, k):
          print("DIR", base, sorted(os.path.basename(f) for f in glob.glob(base + "/*") if f.lower().endswith((".dll", ".pyd"))))
          for f in sorted(glob.glob(base + "/*.dll")) + sorted(glob.glob(base + "/_multiarray_umath*.pyd")):
              try:
                  ctypes.WinDLL(f, winmode=0x00000100 | 0x00001000); print("OK", os.path.basename(f))
              except OSError as e:
                  print("FAIL", os.path.basename(f), e)
    PYTHON
    system Formula["python@3.14"].opt_libexec/"bin/python", "-c", diag
    pythons.each do |python|
      python3 = python.opt_libexec/"bin/python"
      system python3, "-c", <<~PYTHON
        import numpy as np
        t = np.ones((3,3), int)
        assert t.sum() == 9
        assert np.dot(t, t).sum() == 27
      PYTHON
    end
  end
end
