class Mlpack < Formula
  desc "Scalable C++ machine learning library"
  homepage "https://www.mlpack.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mlpack-4.8.0-1-any.pkg.tar.zst"
  sha256 "940b8f2fdee0f5e9044f7bc34c9182c16eb79a280ffc4d1fb2f1d91d0a398d11"
  license all_of: ["BSD-3-Clause", "MPL-2.0", "BSL-1.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://www.mlpack.org/download.html"
    regex(/href=.*?mlpack[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-armadillo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-armadillo-15.6.0-1-any.pkg.tar.zst"
    sha256 "e52b67e9a3604ba4f2436f761567a99a6a7c390c27d4488f6a8e3257eaf948ac"
  end

  resource "mingw-w64-ucrt-x86_64-arpack" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-arpack-3.9.1-9-any.pkg.tar.zst"
    sha256 "82ae1ae5696556faea65d36fb782c270d2e17db34eea8f33bc2c7f90ff0b0d03"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-cereal" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cereal-1.3.2-2-any.pkg.tar.zst"
    sha256 "c1a9a2396081f9db236538f8846264f96d69b91b06759d7526ba4256501ab2e7"
  end

  resource "mingw-w64-ucrt-x86_64-ensmallen" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ensmallen-2.22.2-1-any.pkg.tar.zst"
    sha256 "8e07cf7f8ed3c4854ecee8bb1ca140d7d5331e884eb60599a580b0ac5259977a"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libgfortran" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgfortran-16.2.0-6-any.pkg.tar.zst"
    sha256 "34406a0c721e44655c3971ce131efd81652d5f6fa5381eb5700b288bc9a780ab"
  end

  resource "mingw-w64-ucrt-x86_64-libgomp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgomp-16.2.0-6-any.pkg.tar.zst"
    sha256 "809b95086d8d3ba145e041bb36d5756b4407d1d97c0524ef731d3b94548faa69"
  end

  resource "mingw-w64-ucrt-x86_64-libquadmath" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libquadmath-16.2.0-6-any.pkg.tar.zst"
    sha256 "7b553c0b6dd9cf2f676c0c6812aa359495940dbdd7347f47be1dbd75d29cd611"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-openblas" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openblas-0.3.34-1-any.pkg.tar.zst"
    sha256 "d9e14c9440d9aa70b6d09cdf0b8117995d181ced9c3d233ad7b62a907982add5"
  end

  resource "mingw-w64-ucrt-x86_64-stb" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-stb-r2216.f1c79c0-2-any.pkg.tar.zst"
    sha256 "d38768ebef6e7ba2d4edd1a5ddfd958823ec98079253b33592025cab3548bc69"
  end

  resource "mingw-w64-ucrt-x86_64-superlu" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-superlu-7.0.1-1-any.pkg.tar.zst"
    sha256 "2859dcdd7e297ff501434c2bff1a38bdfc22bb47d0a4877e47eb234ee1e0ebe2"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mlpack_adaboost.exe", "mlpack_adaboost"
    msys2_bin "ucrt64/bin/mlpack_approx_kfn.exe", "mlpack_approx_kfn"
    msys2_bin "ucrt64/bin/mlpack_bayesian_linear_regression.exe", "mlpack_bayesian_linear_regression"
    msys2_bin "ucrt64/bin/mlpack_cf.exe", "mlpack_cf"
    msys2_bin "ucrt64/bin/mlpack_dbscan.exe", "mlpack_dbscan"
    msys2_bin "ucrt64/bin/mlpack_decision_tree.exe", "mlpack_decision_tree"
    msys2_bin "ucrt64/bin/mlpack_det.exe", "mlpack_det"
    msys2_bin "ucrt64/bin/mlpack_emst.exe", "mlpack_emst"
    msys2_bin "ucrt64/bin/mlpack_fastmks.exe", "mlpack_fastmks"
    msys2_bin "ucrt64/bin/mlpack_gmm_generate.exe", "mlpack_gmm_generate"
    msys2_bin "ucrt64/bin/mlpack_gmm_probability.exe", "mlpack_gmm_probability"
    msys2_bin "ucrt64/bin/mlpack_gmm_train.exe", "mlpack_gmm_train"
    msys2_bin "ucrt64/bin/mlpack_hmm_generate.exe", "mlpack_hmm_generate"
    msys2_bin "ucrt64/bin/mlpack_hmm_loglik.exe", "mlpack_hmm_loglik"
    msys2_bin "ucrt64/bin/mlpack_hmm_train.exe", "mlpack_hmm_train"
    msys2_bin "ucrt64/bin/mlpack_hmm_viterbi.exe", "mlpack_hmm_viterbi"
    msys2_bin "ucrt64/bin/mlpack_hoeffding_tree.exe", "mlpack_hoeffding_tree"
    msys2_bin "ucrt64/bin/mlpack_image_converter.exe", "mlpack_image_converter"
    msys2_bin "ucrt64/bin/mlpack_kde.exe", "mlpack_kde"
    msys2_bin "ucrt64/bin/mlpack_kernel_pca.exe", "mlpack_kernel_pca"
    msys2_bin "ucrt64/bin/mlpack_kfn.exe", "mlpack_kfn"
    msys2_bin "ucrt64/bin/mlpack_kmeans.exe", "mlpack_kmeans"
    msys2_bin "ucrt64/bin/mlpack_knn.exe", "mlpack_knn"
    msys2_bin "ucrt64/bin/mlpack_krann.exe", "mlpack_krann"
    msys2_bin "ucrt64/bin/mlpack_lars.exe", "mlpack_lars"
    msys2_bin "ucrt64/bin/mlpack_linear_regression.exe", "mlpack_linear_regression"
    msys2_bin "ucrt64/bin/mlpack_linear_svm.exe", "mlpack_linear_svm"
    msys2_bin "ucrt64/bin/mlpack_lmnn.exe", "mlpack_lmnn"
    msys2_bin "ucrt64/bin/mlpack_local_coordinate_coding.exe", "mlpack_local_coordinate_coding"
    msys2_bin "ucrt64/bin/mlpack_logistic_regression.exe", "mlpack_logistic_regression"
    msys2_bin "ucrt64/bin/mlpack_lsh.exe", "mlpack_lsh"
    msys2_bin "ucrt64/bin/mlpack_mean_shift.exe", "mlpack_mean_shift"
    msys2_bin "ucrt64/bin/mlpack_nbc.exe", "mlpack_nbc"
    msys2_bin "ucrt64/bin/mlpack_nca.exe", "mlpack_nca"
    msys2_bin "ucrt64/bin/mlpack_nmf.exe", "mlpack_nmf"
    msys2_bin "ucrt64/bin/mlpack_pca.exe", "mlpack_pca"
    msys2_bin "ucrt64/bin/mlpack_perceptron.exe", "mlpack_perceptron"
    msys2_bin "ucrt64/bin/mlpack_preprocess_binarize.exe", "mlpack_preprocess_binarize"
    msys2_bin "ucrt64/bin/mlpack_preprocess_describe.exe", "mlpack_preprocess_describe"
    msys2_bin "ucrt64/bin/mlpack_preprocess_imputer.exe", "mlpack_preprocess_imputer"
    msys2_bin "ucrt64/bin/mlpack_preprocess_one_hot_encoding.exe", "mlpack_preprocess_one_hot_encoding"
    msys2_bin "ucrt64/bin/mlpack_preprocess_scale.exe", "mlpack_preprocess_scale"
    msys2_bin "ucrt64/bin/mlpack_preprocess_split.exe", "mlpack_preprocess_split"
    msys2_bin "ucrt64/bin/mlpack_radical.exe", "mlpack_radical"
    msys2_bin "ucrt64/bin/mlpack_random_forest.exe", "mlpack_random_forest"
    msys2_bin "ucrt64/bin/mlpack_range_search.exe", "mlpack_range_search"
    msys2_bin "ucrt64/bin/mlpack_softmax_regression.exe", "mlpack_softmax_regression"
    msys2_bin "ucrt64/bin/mlpack_sparse_coding.exe", "mlpack_sparse_coding"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mlpack_adaboost --version 2>&1")
  end
end
