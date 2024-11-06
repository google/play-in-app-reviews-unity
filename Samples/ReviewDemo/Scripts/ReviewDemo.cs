// Copyright 2019 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Google.Play.Review.Samples.ReviewDemo
{
    public class ReviewDemo : MonoBehaviour
    {
        private static PlayReviewInfo _playReviewInfo;
        private ReviewManager _reviewManager;
        private Dictionary<KeyCode, Action> _keyMappings;
        public Text statusText;
        public Button requestFlowButton;
        public Button launchFlowButton;
        public Button allInOneButton;

        private void Start()
        {
            _reviewManager = new ReviewManager();
            requestFlowButton.onClick.AddListener(RequestFlowClick);
            launchFlowButton.onClick.AddListener(LaunchFlowClick);
            allInOneButton.onClick.AddListener(AllInOneFlowClick);
            requestFlowButton.interactable = true;
            launchFlowButton.interactable = false;
            allInOneButton.interactable = true;

            // Provides an interface to test via key press.
            _keyMappings = new Dictionary<KeyCode, Action>()
            {
                {KeyCode.A, AllInOneFlowClick}
            };
            Debug.Log("Initialized key mapping");
        }

        private void Update()
        {
            if (!Input.anyKeyDown)
            {
                return;
            }

            foreach (var keyMapping in _keyMappings)
            {
                if (Input.GetKeyDown(keyMapping.Key))
                {
                    keyMapping.Value.Invoke();
                }
            }
        }

        private void RequestFlowClick()
        {
            StartCoroutine(RequestFlowCoroutine(true));
        }

        private void LaunchFlowClick()
        {
            StartCoroutine(LaunchFlowCoroutine());
        }

        private void AllInOneFlowClick()
        {
            StartCoroutine(AllInOneFlowCoroutine());
        }

        private IEnumerator RequestFlowCoroutine(bool isStepRequest)
        {
            Debug.Log("Initializing in-app review request flow");
            allInOneButton.interactable = false;
            requestFlowButton.interactable = false;
            statusText.text = "Requesting info...";
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var requestFlowOperation = _reviewManager.RequestReviewFlow();
            yield return requestFlowOperation;
            stopWatch.Stop();
            if (requestFlowOperation.Error != ReviewErrorCode.NoError)
            {
                ResetDisplay(requestFlowOperation.Error.ToString());
                yield break;
            }

            _playReviewInfo = requestFlowOperation.GetResult();
            statusText.text += "\nObtained info! (" + stopWatch.ElapsedMilliseconds + "ms)";
            if (isStepRequest)
            {
                launchFlowButton.interactable = true;
            }
        }

        private IEnumerator LaunchFlowCoroutine()
        {
            launchFlowButton.interactable = false;
            // Gives enough time to the UI to progress to full dimmed (not interactable) LaunchFlowButton
            // before the in-app review dialog is shown.
            yield return new WaitForSeconds(.1f);
            if (_playReviewInfo == null)
            {
                ResetDisplay("PlayReviewInfo is null.");
                yield break;
            }

            yield return statusText.text += "\nLaunching...";
            var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
            yield return launchFlowOperation;
            _playReviewInfo = null;
            if (launchFlowOperation.Error != ReviewErrorCode.NoError)
            {
                ResetDisplay(launchFlowOperation.Error.ToString());
                yield break;
            }

            Debug.Log("In-app review launch is done!");
            statusText.text += "\nDone!";
            ResetDisplay(string.Empty);
        }

        private IEnumerator AllInOneFlowCoroutine()
        {
            yield return StartCoroutine(RequestFlowCoroutine(false));
            yield return StartCoroutine(LaunchFlowCoroutine());
        }

        private void ResetDisplay(string errorText)
        {
            if (!string.IsNullOrEmpty(errorText))
            {
                Debug.LogError(errorText);
                statusText.text += "\nError: " + errorText;
            }

            requestFlowButton.interactable = true;
            allInOneButton.interactable = true;
        }
    }
}