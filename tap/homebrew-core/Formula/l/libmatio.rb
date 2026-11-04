class Libmatio < Formula
  desc "C library for reading and writing MATLAB MAT files"
  homepage "https://matio.sourceforge.net/"
  url "https://github.com/tbeu/matio/releases/download/v1.6.0/matio-1.6.0.tar.gz"
  sha256 "1481dc74e01d249e90f064333f969369fe3dd11d6cc6963e8635c581aa5d7711"
  license "BSD-2-Clause"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "111132d391a649fdff2fa433dc999041221911a86805d383a5025bfce6a5c7f8"
  end

  depends_on "pkgconf" => :test
  depends_on "hdf5"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  resource "homebrew-test_mat_file", :test do
    url "https://web.uvic.ca/~monahana/eos225/poc_data.mat.sfx"
    sha256 "a29df222605476dcfa660597a7805176d7cb6e6c60413a3e487b62b6dbf8e6fe"
  end

  deny_network_access!

  def install
    args = %W[
      --enable-extended-sparse=yes
      --enable-mat73=yes
      --with-hdf5=#{formula_opt_prefix("hdf5")}
    ]
    args << "--with-zlib=#{formula_opt_prefix("zlib-ng-compat")}" unless OS.mac?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    testpath.install resource("homebrew-test_mat_file")
    (testpath/"mat.c").write <<~C
      #include <stdlib.h>
      #include <matio.h>

      size_t dims[2] = {5, 5};
      double data[25] = {0.0, };
      mat_t *mat;
      matvar_t *matvar;

      int main(int argc, char **argv) {
        if (!(mat = Mat_Open(argv[1], MAT_ACC_RDONLY)))
          abort();
        Mat_Close(mat);

        mat = Mat_CreateVer("test_writenan.mat", NULL, MAT_FT_DEFAULT);
        if (mat) {
          matvar = Mat_VarCreate("foo", MAT_C_DOUBLE, MAT_T_DOUBLE, 2,
                                 dims, data, MAT_F_DONT_COPY_DATA);
          Mat_VarWrite(mat, matvar, MAT_COMPRESSION_NONE);
          Mat_VarFree(matvar);
          Mat_Close(mat);
        } else {
          abort();
        }
        mat = Mat_CreateVer("foo", NULL, MAT_FT_MAT73);
        return EXIT_SUCCESS;
      }
    C
    system ENV.cc, "mat.c", "-o", "mat", "-I#{include}", "-L#{lib}", "-lmatio"
    system "./mat", "poc_data.mat.sfx"

    refute_includes "-I/usr/include", shell_output("pkgconf --cflags matio")
  end
end
