class MlxLm < Formula
  include Language::Python::Virtualenv

  desc "Run LLMs with MLX"
  homepage "https://github.com/ml-explore/mlx-lm"
  url "https://files.pythonhosted.org/packages/e1/46/eff1c17019b82912020d4d716527e6fcf4ac392b55cddef03eeb60566ab4/mlx_lm-0.32.2.tar.gz"
  sha256 "1af8a92f6a141c3e2bf5b0369ac4464203a8e2189c47b9e40d3e921e5b3a1fe9"
  license "MIT"

  # MLX ships official Windows builds on PyPI: the mlx wheel plus mlx-cpu (mlx.dll, OpenBLAS) run
  # MLX on the CPU backend. mlx-lm itself is pure Python; its native dependencies are the official
  # win_amd64 wheels for Python 3.14, so nothing is compiled.

  livecheck do
    url :stable
  end

  depends_on "python@3.14"

  resource "annotated-doc" do
    url "https://files.pythonhosted.org/packages/3e/30/e900b21425a860e195f32e37657aa1f7c7f2b1bfb26f03ca209b90933c06/annotated_doc-0.0.5-py3-none-any.whl"
    sha256 "117bac03a25ede5df5440e855b32d556049ca169ead221505badf432fed4b101"
  end

  resource "anyio" do
    url "https://files.pythonhosted.org/packages/12/b8/4bd346e22b28902df4d651910f5242c28d84e4a5c2435ca5c3f797ed7e2e/anyio-4.15.1-py3-none-any.whl"
    sha256 "6152fdbbf9a77fdec97731721bebf7c4c44f7c29b424b0065826173efc7ed101"
  end

  resource "click" do
    url "https://files.pythonhosted.org/packages/58/50/6c0d534c5f134586a8e1ba4e330569e32f057e33372ae556463212fb4cd3/click-8.5.0-py3-none-any.whl"
    sha256 "255bc9599cf7748b4b1a446ccc735421bd08a2ae529a8b88597d3de5664ee360"
  end

  resource "colorama" do
    url "https://files.pythonhosted.org/packages/d1/d6/3965ed04c63042e047cb6a3e6ed1a63a35087b6a609aa3a15ed8ac56c221/colorama-0.4.6-py2.py3-none-any.whl"
    sha256 "4f1d9991f5acc0ca119f9d443620b77f9d6b33703e51011c16baf57afb285fc6"
  end

  resource "filelock" do
    url "https://files.pythonhosted.org/packages/8e/a3/9bc26acff301fe1aaea1cc3d82a1d57e0a34df3e1cadbfa91ac2dbcdde5c/filelock-4.2.0-py3-none-any.whl"
    sha256 "2ff5690882e8cdb00ef31fb3d01a3094c29f30985426c59495afb1733f3b7238"
  end

  resource "fsspec" do
    url "https://files.pythonhosted.org/packages/6c/c0/a98505f18594f1bce828bb159cec0fcf9860562f1a2c85913409fc8f3d9e/fsspec-2026.9.0-py3-none-any.whl"
    sha256 "8dd6e646e99ea382bd85f97a45e6b526a442d79423a7dc673f1e2756d05fcb5f"
  end

  resource "h11" do
    url "https://files.pythonhosted.org/packages/04/4b/29cac41a4d98d144bf5f6d33995617b185d14b22401f75ca86f384e87ff1/h11-0.16.0-py3-none-any.whl"
    sha256 "63cf8bbe7522de3bf65932fda1d9c2772064ffb3dae62d55932da54b31cb6c86"
  end

  resource "hf-xet" do
    url "https://files.pythonhosted.org/packages/4d/59/b35106dfa71b6eef605dc88bd038fe99c7f86fb132a15b60d0bf2f235b2c/hf_xet-1.7.0-cp38-abi3-win_amd64.whl"
    sha256 "2b7bb5727889b0f2436dbaaad8fc4c3e66b8240d992716989e0c086b4278b1bc"
  end

  resource "httpcore2" do
    url "https://files.pythonhosted.org/packages/09/ba/a4568248771ce81957bfb7cc600264a40fbcda092391ee1c415c50be4bea/httpcore2-2.13.1-py3-none-any.whl"
    sha256 "e1e05d4f25f7d7d496bfb96748f6f4b67657b03da069b3a68c36069f3db73d0a"
  end

  resource "httpx2" do
    url "https://files.pythonhosted.org/packages/d8/9c/6fe8931fd9f381042a9e4c7d5a7b4cbf7016b252bec0c99a49fce42c3326/httpx2-2.13.1-py3-none-any.whl"
    sha256 "6dff50fabc270ee5fd25d845d0b078ed20564579744d6d962850975996d2f9a4"
  end

  resource "huggingface-hub" do
    url "https://files.pythonhosted.org/packages/eb/b0/0f7b430fd100b3a3b037fdbb314878200241082e607b3383c63d91a13a72/huggingface_hub-2.2.0-py3-none-any.whl"
    sha256 "1667f145dc56dc210d60966069397df9ecfca9607a5d43db88b308c89dae56b3"
  end

  resource "idna" do
    url "https://files.pythonhosted.org/packages/58/a2/bb081bab032533a855d44de1d56f8e8426114ff1ba5d1f07a438a0a654f8/idna-3.20-py3-none-any.whl"
    sha256 "ab7ae7122974553370f0bdb919e1a960b2cd1bc1ef0276416d896db81c14582c"
  end

  resource "jinja2" do
    url "https://files.pythonhosted.org/packages/62/a1/3d680cbfd5f4b8f15abc1d571870c5fc3e594bb582bc3b64ea099db13e56/jinja2-3.1.6-py3-none-any.whl"
    sha256 "85ece4451f492d0c13c5dd7c13a64681a86afae63a5f347908daf103ce6d2f67"
  end

  resource "markdown-it-py" do
    url "https://files.pythonhosted.org/packages/b3/81/4da04ced5a082363ecfa159c010d200ecbd959ae410c10c0264a38cac0f5/markdown_it_py-4.2.0-py3-none-any.whl"
    sha256 "9f7ebbcd14fe59494226453aed97c1070d83f8d24b6fc3a3bcf9a38092641c4a"
  end

  resource "markupsafe" do
    url "https://files.pythonhosted.org/packages/80/f0/5792ff768a410f93ee3f84fc19345295ffc352d2c936b424cb37e514714c/markupsafe-3.0.4-cp314-cp314-win_amd64.whl"
    sha256 "c1bc67752d5f21013cfe430df4062441714eab79f65a6a05e01505957e9c35fe"
  end

  resource "mdurl" do
    url "https://files.pythonhosted.org/packages/b3/38/89ba8ad64ae25be8de66a6d463314cf1eb366222074cfda9ee839c56a4b4/mdurl-0.1.2-py3-none-any.whl"
    sha256 "84008a41e51615a49fc9966191ff91509e3c40b939176e643fd50a5c2196b8f8"
  end

  resource "mlx" do
    url "https://files.pythonhosted.org/packages/67/e7/c5114cc6e5e895fc1ad3acb4069345bc8540002c610764d8f721c047aa39/mlx-0.32.4-cp314-cp314-win_amd64.whl"
    sha256 "001b07946f2867a800aee73d302ffdaf5eb81cafe64c7ba19fddf7e219324822"
  end

  resource "mlx-cpu" do
    url "https://files.pythonhosted.org/packages/d0/c9/308e516701fecadb7b602a617cbff66cb1d3a543b6b8c205d9422a789f4f/mlx_cpu-0.32.4-py3-none-win_amd64.whl"
    sha256 "1631a01b3017c6ee846b8080d9b3fc762fd016dea15a843ee9b0c64cfda5b73f"
  end

  resource "numpy" do
    url "https://files.pythonhosted.org/packages/52/9a/e1e512ebc948d5b9dd33b08736760f0ebbed2848fd4eda1f553088a6dcee/numpy-2.5.4-cp314-cp314-win_amd64.whl"
    sha256 "2ca144f15135b6212a5c47b1e2aeca6e412f102f95a2d5d88d8aec77eb255de3"
  end

  resource "packaging" do
    url "https://files.pythonhosted.org/packages/63/34/ba1c580383c9eada3711951fef0795c80b829a078d72188184bcab9dd527/packaging-26.3-py3-none-any.whl"
    sha256 "d7193f7c8e4e93f444fde0262bf90af30e16fa0ad0ad44cb553c87339b23cd1c"
  end

  resource "protobuf" do
    url "https://files.pythonhosted.org/packages/8a/55/b77bda4e5e5f5971fb51b07663694690e9afdb9402136c16a522bd621cad/protobuf-7.36.2-cp310-abi3-win_amd64.whl"
    sha256 "a300819d441e078a5608c0d3c709796bb548136058fda017ae51d425b44fd353"
  end

  resource "pygments" do
    url "https://files.pythonhosted.org/packages/71/46/17f022dd3e953bf20a04a028a21ec746d942f8d2af30fa0f124fa0e6a684/pygments-2.21.0-py3-none-any.whl"
    sha256 "2363c69b61c4a97c838da3b130dcd6468f4848992b21a82f2a63ec34377137d9"
  end

  resource "pyyaml" do
    url "https://files.pythonhosted.org/packages/23/20/bb6982b26a40bb43951265ba29d4c246ef0ff59c9fdcdf0ed04e0687de4d/pyyaml-6.0.3-cp314-cp314-win_amd64.whl"
    sha256 "4a2e8cebe2ff6ab7d1050ecd59c25d4c8bd7e6f400f5f82b96557ac0abafd0ac"
  end

  resource "regex" do
    url "https://files.pythonhosted.org/packages/31/e2/c2b466924ccbeb874862968ca638051b15a8fd29d994a0e99004a5cbf78e/regex-2026.9.29-cp314-cp314-win_amd64.whl"
    sha256 "1c2a0026062abcc321a53db4a185ceba0b59a66b5d37b0808917a88b55a5257f"
  end

  resource "rich" do
    url "https://files.pythonhosted.org/packages/82/3b/64d4899d73f91ba49a8c18a8ff3f0ea8f1c1d75481760df8c68ef5235bf5/rich-15.0.0-py3-none-any.whl"
    sha256 "33bd4ef74232fb73fe9279a257718407f169c09b78a87ad3d296f548e27de0bb"
  end

  resource "safetensors" do
    url "https://files.pythonhosted.org/packages/1b/6d/3fba214c1e5e0f69991677ec3bc17023f0421776975e1de0c682dca475e2/safetensors-0.8.0-cp310-abi3-win_amd64.whl"
    sha256 "096ec1a98435df7beb08853bb5aa9081a84f23d0adc67ed1a0a10550f608373f"
  end

  resource "sentencepiece" do
    url "https://files.pythonhosted.org/packages/33/fe/4906f12c458274edd96387e4baaad7c6f064a2b7c11a1cc2401c8a7bd483/sentencepiece-0.2.2-cp314-cp314-win_amd64.whl"
    sha256 "63250cfab8b80a1ef82a614eb2b3cadfec2c405f870cedc139d08e2f063eb708"
  end

  resource "shellingham" do
    url "https://files.pythonhosted.org/packages/e0/f9/0595336914c5619e5f28a1fb793285925a8cd4b432c9da0a987836c7f822/shellingham-1.5.4-py2.py3-none-any.whl"
    sha256 "7ecfff8f2fd72616f7481040475a65b2bf8af90a56c89140852d1120324e8686"
  end

  resource "tokenizers" do
    url "https://files.pythonhosted.org/packages/d1/6c/24f074c9a0efb98e61b20aafe6b2641922d5db24e447d5d6daffd9e17555/tokenizers-0.23.3-cp310-abi3-win_amd64.whl"
    sha256 "1f0823bb00c5fdc98e487354d54dd55a03848d61a1a0bf29a68c77f24f3b26c3"
  end

  resource "tqdm" do
    url "https://files.pythonhosted.org/packages/a7/03/921a3d3c75785aca9ebfbfcabfbc3a1be12e2ab5265deb026d55a5a3f83e/tqdm-4.70.1-py3-none-any.whl"
    sha256 "c293e525e6fef9c20e8728fd4612df02a0aa31bb5fe91ecd93e123b1b7bffa73"
  end

  resource "transformers" do
    url "https://files.pythonhosted.org/packages/ae/ba/057af605774c3c7a9228ce61d88040749134ef320426403b31bb73755d43/transformers-5.20.1-py3-none-any.whl"
    sha256 "5f51ce444193e5d0a751e3fa489ddbc2224ff12f6675a5cfefd4044b09757c19"
  end

  resource "truststore" do
    url "https://files.pythonhosted.org/packages/51/e9/3a7820be2bb0fe53b6bc9c3be26d3d1158004e4c3ab953aa6840b955b1e9/truststore-0.10.5-py3-none-any.whl"
    sha256 "9aaaedaefaf06d8b206278cf8b5012bc897f485a874503501e12d776df78951c"
  end

  resource "typer" do
    url "https://files.pythonhosted.org/packages/07/ea/2e31b67051e91a133189e9c000c222502ddc6969856416de0d095de4c0b0/typer-0.27.3-py3-none-any.whl"
    sha256 "e50022f28b82a86313e54501317a1db64bf8f8d036ff8cfe5ca7e47675454aff"
  end

  resource "typing-extensions" do
    url "https://files.pythonhosted.org/packages/49/d3/b8441a820a491ddfc024b0b0cf0393375b75ea13866d9c66727e54c2fc80/typing_extensions-4.16.0-py3-none-any.whl"
    sha256 "481caa481374e813c1b176ada14e97f1f67a4539ce9cfeb3f350d78d6370c2e8"
  end

  def install
    # mlx_lm raises RLIMIT_NOFILE through the Unix-only `resource` module; Windows has no
    # per-process open-file limit to raise.
    inreplace "mlx_lm/utils.py" do |s|
      s.sub! "import resource
", ""
      s.sub! "resource.setrlimit(resource.RLIMIT_NOFILE, (2048, 4096))",
             "# No RLIMIT_NOFILE on Windows (no per-process open-file limit to raise)"
    end
    virtualenv_install_with_resources
  end

  def caveats
    <<~EOS
      MLX runs on the CPU on Windows (Metal is Apple-only), so generation is slower than on an
      Apple silicon Mac. Models download from Hugging Face on first use, e.g.
        mlx_lm.generate --model mlx-community/SmolLM2-135M-Instruct-8bit --prompt "Hello"
    EOS
  end

  test do
    assert_match "usage: mlx_lm.generate", shell_output("#{bin}/mlx_lm.generate --help")
    output = pipe_output("#{libexec}/bin/python -", "import mlx.core as mx
print(mx.sum(mx.array([1.0, 2.0, 3.0]) ** 2).item())
", 0)
    assert_match "14.0", output

    port = free_port
    pid = spawn bin/"mlx_lm.server", "--port=#{port}"
    sleep 15
    assert_match '"status": "ok"', shell_output("curl --silent --retry 5 --retry-connrefused localhost:#{port}/health")
  ensure
    Process.kill("TERM", pid) if pid
  end
end
